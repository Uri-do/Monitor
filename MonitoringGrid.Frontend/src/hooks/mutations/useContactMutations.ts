import { useMutation, useQueryClient } from '@tanstack/react-query';
import { contactApi } from '@/services/api';
import { queryKeys } from '@/utils/queryKeys';
import { CreateContactRequest, UpdateContactRequest } from '@/types/api';
import toast from 'react-hot-toast';

/**
 * Hook for creating a new contact
 */
export const useCreateContact = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateContactRequest) => contactApi.createContact(data),
    onSuccess: newContact => {
      toast.success('Contact created successfully');

      // Invalidate contact lists to show the new contact
      queryClient.invalidateQueries({ queryKey: queryKeys.contacts.lists() });

      // Optionally set the new contact in cache
      queryClient.setQueryData(queryKeys.contacts.detail(newContact.contactID), newContact);
    },
    onError: (error: any) => {
      toast.error(error.message || 'Failed to create contact');
    },
  });
};

/**
 * Hook for updating an existing contact
 */
export const useUpdateContact = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: UpdateContactRequest) => contactApi.updateContact(data),
    onSuccess: (updatedContact, variables) => {
      toast.success('Contact updated successfully');

      // Update the specific contact in cache
      queryClient.setQueryData(queryKeys.contacts.detail(variables.contactID), updatedContact);

      // Invalidate contact lists to reflect changes
      queryClient.invalidateQueries({ queryKey: queryKeys.contacts.lists() });

      // If contact is used in Indicators, invalidate Indicator data too
      queryClient.invalidateQueries({ queryKey: queryKeys.indicators.lists() });
    },
    onError: (error: any) => {
      toast.error(error.message || 'Failed to update contact');
    },
  });
};

/**
 * Hook for deleting a contact
 */
export const useDeleteContact = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (contactId: number) => contactApi.deleteContact(contactId),
    onSuccess: (_, contactId) => {
      toast.success('Contact deleted successfully');

      // Remove the contact from cache
      queryClient.removeQueries({ queryKey: queryKeys.contacts.detail(contactId) });

      // Invalidate contact lists
      queryClient.invalidateQueries({ queryKey: queryKeys.contacts.lists() });

      // Invalidate Indicator data as contact assignments may have changed
      queryClient.invalidateQueries({ queryKey: queryKeys.indicators.lists() });
    },
    onError: (error: any) => {
      toast.error(error.message || 'Failed to delete contact');
    },
  });
};

/**
 * Hook for bulk contact operations
 */
export const useBulkContactOperation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: contactApi.bulkOperation,
    onSuccess: (_, variables) => {
      const operationType = variables.operation;
      const count = variables.contactIds.length;

      toast.success(`${count} contact${count > 1 ? 's' : ''} ${operationType} successfully`);

      // Invalidate all contact-related queries
      queryClient.invalidateQueries({ queryKey: queryKeys.contacts.all });

      // If contacts are used in Indicators, invalidate Indicator data too
      queryClient.invalidateQueries({ queryKey: queryKeys.indicators.lists() });
    },
    onError: (error: any) => {
      toast.error(error.message || 'Failed to perform bulk operation');
    },
  });
};
